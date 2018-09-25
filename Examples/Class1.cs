using System;

public class Class1
{
    public Class1() { }
    private double _number;
    public double Number { get { return this._number; } set { this._number = value; } }
    public void Clear() { this.DoClear(); }
    private void DoClear() { this._number = 0; }
    public double Add(double number) { return (this._number += number); }
    public static double Pi { get { return Math.PI; } }
    public static double GetPi() { return Pi; }
    public int add(Person x, Person y) { return x.age + y.age; }
}
