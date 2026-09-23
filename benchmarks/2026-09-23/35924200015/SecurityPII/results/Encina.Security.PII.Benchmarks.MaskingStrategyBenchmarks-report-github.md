```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.75GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  95.83 ns |  0.886 ns | 0.528 ns |  1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 425.38 ns |  1.406 ns | 0.930 ns |  4.44 |    0.02 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 500.04 ns |  1.028 ns | 0.612 ns |  5.22 |    0.03 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 413.69 ns |  1.912 ns | 1.265 ns |  4.32 |    0.03 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 187.59 ns |  1.447 ns | 0.957 ns |  1.96 |    0.01 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 115.46 ns |  1.302 ns | 0.861 ns |  1.20 |    0.01 | 0.0130 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 233.64 ns |  1.261 ns | 0.750 ns |  2.44 |    0.01 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 181.10 ns |  0.645 ns | 0.427 ns |  1.89 |    0.01 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  54.29 ns |  0.566 ns | 0.337 ns |  0.57 |    0.00 | 0.0051 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  66.06 ns |  0.457 ns | 0.272 ns |  0.69 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 104.41 ns |  1.691 ns | 1.118 ns |  1.09 |    0.01 | 0.0130 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 599.49 ns |  3.223 ns | 2.132 ns |  6.26 |    0.04 | 0.0162 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  94.79 ns | 17.377 ns | 0.952 ns |  1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 432.94 ns | 24.891 ns | 1.364 ns |  4.57 |    0.04 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 499.92 ns | 15.182 ns | 0.832 ns |  5.27 |    0.05 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 425.59 ns | 73.329 ns | 4.019 ns |  4.49 |    0.05 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 187.64 ns | 14.917 ns | 0.818 ns |  1.98 |    0.02 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 119.10 ns | 29.646 ns | 1.625 ns |  1.26 |    0.02 | 0.0129 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 242.32 ns | 11.843 ns | 0.649 ns |  2.56 |    0.02 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 181.69 ns | 10.777 ns | 0.591 ns |  1.92 |    0.02 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.04 ns | 15.653 ns | 0.858 ns |  0.57 |    0.01 | 0.0051 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  65.13 ns |  3.543 ns | 0.194 ns |  0.69 |    0.01 | 0.0050 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 103.96 ns |  2.647 ns | 0.145 ns |  1.10 |    0.01 | 0.0130 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 581.29 ns |  8.593 ns | 0.471 ns |  6.13 |    0.05 | 0.0162 |     416 B |        1.86 |
