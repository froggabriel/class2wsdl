using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Class1
/// </summary>

public class Person
{
    public int age { get { return age; } set { age = value; } }
    public string name { get { return name; } set { name = value; } }
    private double? height
    {
        get { return age; }
        set { height = value; }
    }
    private double? weight
    {
        get { return age; }
        set { weight = value; }
    }

    public Person()
    {
        age = 0;
        name = "";
        height = 0;
        weight = 0;
    }
    public Person(int age, string name, double? height, double? weight)
    {
        this.age = age;
        this.name = name;
        this.height = height;
        this.weight = weight;
    }

    public double getBMI()
    {
        if (height != null && weight != null)
        {
            return (double)weight / Math.Pow((double)height, 2);
        }
        else
        {
            return 0;
        }
    }
}