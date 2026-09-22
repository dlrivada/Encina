```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.38GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 103.63 ns |  0.989 ns | 0.654 ns |  1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 447.15 ns |  4.497 ns | 2.676 ns |  4.31 |    0.04 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 515.79 ns |  1.059 ns | 0.630 ns |  4.98 |    0.03 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 428.28 ns |  1.247 ns | 0.825 ns |  4.13 |    0.03 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 195.40 ns |  0.568 ns | 0.297 ns |  1.89 |    0.01 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 127.03 ns |  0.605 ns | 0.400 ns |  1.23 |    0.01 | 0.0129 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 246.84 ns |  1.606 ns | 1.062 ns |  2.38 |    0.02 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 189.54 ns |  0.441 ns | 0.292 ns |  1.83 |    0.01 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  58.13 ns |  0.118 ns | 0.070 ns |  0.56 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  70.78 ns |  0.332 ns | 0.220 ns |  0.68 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 118.59 ns |  1.255 ns | 0.830 ns |  1.14 |    0.01 | 0.0129 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 617.70 ns |  6.188 ns | 4.093 ns |  5.96 |    0.05 | 0.0162 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 104.61 ns |  6.617 ns | 0.363 ns |  1.00 |    0.00 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 442.74 ns | 19.993 ns | 1.096 ns |  4.23 |    0.02 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 514.79 ns | 28.833 ns | 1.580 ns |  4.92 |    0.02 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 431.80 ns | 30.346 ns | 1.663 ns |  4.13 |    0.02 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 199.71 ns | 42.966 ns | 2.355 ns |  1.91 |    0.02 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 128.78 ns | 10.346 ns | 0.567 ns |  1.23 |    0.01 | 0.0129 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 252.38 ns |  3.864 ns | 0.212 ns |  2.41 |    0.01 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 189.50 ns | 11.019 ns | 0.604 ns |  1.81 |    0.01 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  58.29 ns |  1.032 ns | 0.057 ns |  0.56 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  69.82 ns |  2.337 ns | 0.128 ns |  0.67 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 116.25 ns | 18.698 ns | 1.025 ns |  1.11 |    0.01 | 0.0129 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 590.43 ns | 27.032 ns | 1.482 ns |  5.64 |    0.02 | 0.0162 |     416 B |        1.86 |
