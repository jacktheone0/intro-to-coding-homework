arr = []
n = int(input("give me a number"))
i = 1
while i < (n+1):
    arr.append(i)
    i=i+1




arr = list(range(1, n + 1))


sum_odd_index = 0

for i in range(len(arr)):
    if i % 2 != 0: 
        sum_odd_index += arr[i]

print(sum_odd_index)

sum_even_index = 0

for i in range(len(arr)):
    if i % 2 == 0: 
        sum_even_index += arr[i]

print(sum_even_index)






user_input = input("Enter a string: ")

vowels = "aeiouAEIOU"

result = ""

for char in user_input:
    if char in vowels:
        result += "@"
    else:
        result += char

print("Modified string:", result)
