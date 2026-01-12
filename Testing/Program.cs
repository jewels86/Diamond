using System.Numerics;
using System.Reflection.Metadata;
using Diamond;
using Jewels.Lazulite;

namespace Testing;

class Program
{
    static void Main(string[] args)
    {
        //HashingTests.TestSHA256();
        //HashingTests.TestSHA512();
        //HashingTests.TestMD5();
        
        //BigIntTests.TestAdd();      // working!
        //BigIntTests.TestSubtract(); // working!
        //BigIntTests.TestMultiply(); // working!
        //BigIntTests.TestDivide();   // working!
        //BigIntTests.TestMod();      // working!
        //BigIntTests.TestGCD();      // working!
        //BigIntTests.TestBarrett();  // working!
        //BigIntTests.TestMonty();    // working!
        //BigIntTests.TestStupids();  // working!
        //BigIntTests.TestStupids2();
        BigIntTests.TestRaphael();
        
        //ConstantTime.Analytics.TestMontgomeryModPow();    // constant time checked
        //ConstantTime.Analytics.TestGCD();
        //ConstantTime.Analytics.TestGCDEvenVsOdd();
    }
}