from config import *
import sys
import time
import math
import random

_b32alphabet = {
    0: 'A',  9: 'K', 18: 'U', 27: '5',
    1: 'B', 10: 'L', 19: 'V', 28: '6',
    2: 'C', 11: 'M', 20: 'W', 29: '7',
    3: 'D', 12: 'N', 21: 'X', 30: '8',
    4: 'E', 13: 'P', 22: 'Y', 31: '9',
    5: 'F', 14: 'Q', 23: 'Z',
    6: 'G', 15: 'R', 24: '2',
    7: 'H', 16: 'S', 25: '3',
    8: 'J', 17: 'T', 26: '4',
    }

_b32tab = _b32alphabet.items()
_b32tab.sort()
_b32tab = [v for k, v in _b32tab]

def CreateRandom(codeNum, max):
    ret = random.sample(xrange(max),codeNum)
    return ret

def Encode(s, len):
    parts = []
    groupNum = len / 5
    for i in range(groupNum):
        temp = s << i*5
        temp = temp >> (groupNum - 1)*5
        temp = temp & 0x1f
        print 'Encode index is', temp,'alphabet is', _b32tab[temp],' bin is', bin(temp) 
        parts.append(_b32tab[temp])
    encoded = ''.join(parts)
    return encoded

def main():
    intTimeStamp = int(time.time())
    print 'timeStamp is',intTimeStamp,'bin is',bin(intTimeStamp)
    timebitNum = len(bin(intTimeStamp)) - 2
    randombitNum = math.ceil(math.log(MaxScope, 2))
    print '1.randombitNum',randombitNum
    totalBitNum = math.ceil((timebitNum + randombitNum) / 5) * 5 
    randombitNum = totalBitNum - timebitNum
    randomMaxNum = math.pow(2, randombitNum) - 1
    randomList = CreateRandom(num, int(randomMaxNum))
    finalNums = []
    moveNum = intTimeStamp<<int(randombitNum)
    for randomNum in randomList:
        print '1.move num is', bin(moveNum)
        print 'random num is', bin(randomNum)
        finalNums.append(moveNum | randomNum)
        print '2.move num is', bin(moveNum | randomNum),'\n'
    finalencodes = []
    for final in finalNums:
        finalencodes.append(Encode(final, int(totalBitNum)))
    print '\n', finalencodes

if __name__ == '__main__':
	main()