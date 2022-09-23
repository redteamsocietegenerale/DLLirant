#!/usr/bin/env python
# coding: utf-8

import os
import sys
import shutil
import time
import argparse
import subprocess
import pefile
import string
import random

PARSER = argparse.ArgumentParser()
PARSER.add_argument('-f', '--file', type=str, help='define the targeted binary (use it alone without -p or -n)', required=False)
PARSER.add_argument('-p', '--proxydll', type=str, help='create a proxy dll (redirecting all the functions to the original DLL)', required=False)
ARGS = PARSER.parse_args()

# The DLL filenames who starts with one element of this list will not be checked.
dlls_excludes = {
	'api-ms',
	'ext-ms',
	'ntdll',
	'kernel32',
	'user32',
	'shell32',
	'comctl32',
	'imm32',
	'gdi32',
	'msvcr',
	'ws2_32',
	'ole32',
	'ninput',
	'setupapi',
	'mscoree',
	'msvcp_win',
	'oleaut32',
	'advapi32',
	'crypt32'
}

def ascii():
	print('·▄▄▄▄  ▄▄▌  ▄▄▌  ▪  ▄▄▄   ▄▄▄·  ▐ ▄ ▄▄▄▄▄')
	print('██▪ ██ ██•  ██•  ██ ▀▄ █·▐█ ▀█ •█▌▐█•██  ')
	print('▐█· ▐█▌██▪  ██▪  ▐█·▐▀▀▄ ▄█▀▀█ ▐█▐▐▌ ▐█.▪')
	print('██. ██ ▐█▌▐▌▐█▌▐▌▐█▌▐█•█▌▐█ ▪▐▌██▐█▌ ▐█▌·')
	print('▀▀▀▀▀• .▀▀▀ .▀▀▀ ▀▀▀.▀  ▀ ▀  ▀ ▀▀ █▪ ▀▀▀  v0.4 - Sh0ck (@Sh0ckFR)')

def rreplace(s, old, new):
	return (s[::-1].replace(old[::-1],new[::-1], 1))[::-1]

def delete_dir(directory):
	if os.path.exists(directory):
		try:
			shutil.rmtree(directory)
		except PermissionError:
			pass

def create_dir(directory):
	if not os.path.exists(directory):
		os.makedirs(directory)

def delete_file(file):
	if os.path.exists(file):
		os.remove(file)

def copy_binary_to_ouput_dir(binary_path):
	if not os.path.exists(binary_path):
		return False
	binary_name = os.path.basename(binary_path).replace(' ', '_')
	try:
		shutil.copyfile(binary_path, f'output/{binary_name}')
		return True
	except FileNotFoundError:
		return False
	except PermissionError:
		return False

def copy_binary_and_required_files(binary):
	copy_binary_to_ouput_dir(binary)
	if os.path.exists('import'):
		for (dirpath, dirnames, filenames) in os.walk('import'):
			for file in filenames:
				copy_binary_to_ouput_dir(f'import/{file}')

def check_if_excluded(dll_name):
	for exclude in dlls_excludes:
		if dll_name.lower().startswith(exclude) or dll_name.upper().startswith(exclude):
			return True
	return False

def get_imports_functions(dll_name, imports):
	functions = []
	for imp in imports:
		if imp.name is not None:
			functions.append(imp.name.decode('utf-8'))
	return functions

def generate_test_dll(functions = None):
	exported_functions = []
	with open('DLLirantDLL\\dllmain-preset.cpp', 'r') as fin:
		with open('DLLirantDLL\\dllmain.cpp', 'w') as fout:
			if functions is not None:
				for line in fin:
					if '##DLL_MAIN##' in line:
						if ARGS.proxydll:
							fout.write(line.replace('##DLL_MAIN##', 'CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);\nbreak;'))
						else:
							fout.write(line.replace('##DLL_MAIN##', ''))
					elif '##EXPORTED_FUNCTIONS##' in line:
						if ARGS.proxydll:
							fout.write(line.replace('##EXPORTED_FUNCTIONS##', functions))
						else:
							for func in functions:
								if len(func) > 0:
									exported_functions.append(f'extern "C" __declspec(dllexport) void {func}()' + '{ Main(); }')
							exported_functions = '\n'.join(exported_functions)
							fout.write(line.replace('##EXPORTED_FUNCTIONS##', exported_functions))
					else:
						fout.write(line)
			else:
				for line in fin:
					if '##DLL_MAIN##' in line:
						fout.write(line.replace('##DLL_MAIN##', 'CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)Main, NULL, NULL, NULL);\nbreak;'))
					elif '##EXPORTED_FUNCTIONS##' in line:
						fout.write(line.replace('##EXPORTED_FUNCTIONS##', ''))
					else:
						fout.write(line)
	os.system('cd DLLirantDLL && clang++ dllmain.cpp -o DLLirantDLL.dll -shared')
	delete_file('DLLirantDLL\\DLLirantDLL.exp')
	delete_file('DLLirantDLL\\DLLirantDLL.lib')
	delete_file('DLLirantDLL\\dllmain.cpp')
	return exported_functions

def check_dll_hijacking(binary_name, binary_original_directory, dll_name, exported_functions = 'DllMain'):
	if not os.path.exists('DLLirantDLL\\DLLirantDLL.dll'):
		return False
	os.system(f'copy DLLirantDLL\\DLLirantDLL.dll output\\{dll_name}')
	delete_file('DLLirantDLL\\DLLirantDLL.dll')
	ascii()
	print('==================================================')
	print(f'[+] Testing {dll_name}')
	print(f'BINARY: {binary_original_directory}\\{binary_name}')
	print(f'EXPORTED FUNCTIONS:\n')
	print(exported_functions)
	print('==================================================')
	try:
		binary_name = binary_name.replace(' ', '_')
		process = subprocess.Popen(f'output/{binary_name}')
		time.sleep(2)
		if os.path.exists('C:\\DLLirant\\output.txt'):
			with open('results.txt', 'a') as file:
				file.write(f'[+] POTENTIAL DLL HIJACKING FOUND IN: {dll_name}\n')
				file.write(f'BINARY: {binary_original_directory}\\{binary_name}\n')
				file.write(f'{exported_functions}\n\n')
			delete_file('C:\\DLLirant\\output.txt')
			input(f'\n\n[+] Potential DLL Hijacking found in the binary {binary_name} with the dll {dll_name} ! Press enter to continue.')
			os.system(f'taskkill /F /pid {process.pid}')
			return True
		os.system(f'taskkill /F /pid {process.pid}')
		return False
	except OSError:
		with open('admin-required.txt', 'a') as file:
			file.write(f'[!] ADMIN PRIVS REQUIRED ON {binary_original_directory}\\{binary_name}\n')
			file.write(f'DLL: {dll_name}\n')
			file.write(f'{exported_functions}\n\n')
		input(f'\n\n[+] [!] Admin privs required on {binary_name} start it manually to test the dll hijack and press enter to continue.')
		return False

def generate_proxy_dll():
	exported_functions = []

	letters = string.ascii_letters
	name_dll = ''.join(random.choice(letters) for i in range(5))
	original_name = os.path.basename(ARGS.proxydll)

	pe = pefile.PE(ARGS.proxydll)

	for entry in pe.DIRECTORY_ENTRY_EXPORT.symbols:
		func = entry.name.decode('utf-8')
		exported_functions.append(f'#pragma comment(linker,"/export:{func}={name_dll}.{func},@{entry.ordinal}")')
	exported_functions = '\n'.join(exported_functions)
	
	ascii()
	generate_test_dll(exported_functions)
	os.system(f'copy DLLirantDLL\\DLLirantDLL.dll output\\DLLirantProxy.dll')
	delete_file('DLLirantDLL\\DLLirantDLL.dll')
	print(f'\n\n[+] Rename the original dll file {name_dll}.dll and copy the compiled dll DLLirantProxy.dll to the original directory as {original_name}')

def main():
	if ARGS.proxydll:
		generate_proxy_dll()
		sys.exit()

	# Create output dir if not exists.
	create_dir('output')

	# Create or recreate the directory used by the DLLirant DLL specified in dllmain-preset.c file.
	delete_dir('C:\\DLLirant')
	create_dir('C:\\DLLirant')
	delete_dir('output')
	create_dir('output')

	# Name of the binary specified and his directory.
	binary_name = os.path.basename(ARGS.file)
	binary_original_directory = os.path.dirname(os.path.realpath(ARGS.file))

	# Copy the binary to the output directory and copy the required files placed by the user in the "import" directory if exists.
	copy_binary_and_required_files(ARGS.file)

	pe = pefile.PE(ARGS.file)
	pe.parse_data_directories()

	# For each dll files...
	for entry in pe.DIRECTORY_ENTRY_IMPORT:
		# Get the name of the dll.
		dll_name = entry.dll.decode('utf-8')

		if check_if_excluded(dll_name) is False:
			# Get the entry import functions.
			functions = get_imports_functions(dll_name, entry.imports)

			# Generate the DLLirant test dll file without exported functions.
			generate_test_dll()

			# Test the generated dll to check if a dll hijacking is possible.
			check_dll_hijacking(binary_name, binary_original_directory, dll_name)

			# Test all functions one by one.
			functions_list = []
			for func in functions:
				functions_list.append(func)
				exported_functions = generate_test_dll(functions_list)
				check_dll_hijacking(binary_name, binary_original_directory, dll_name, exported_functions)

			# Delete and recreate the output directory to test the others dll files.
			delete_dir('output')
			create_dir('output')

			# Recopy the binary and the required files.
			copy_binary_and_required_files(ARGS.file)

if __name__ == '__main__':
	main()
