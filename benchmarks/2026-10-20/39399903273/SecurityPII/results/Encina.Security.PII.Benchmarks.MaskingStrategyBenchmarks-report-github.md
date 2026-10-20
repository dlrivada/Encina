```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  79.29 ns |  1.077 ns | 0.563 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 329.74 ns |  2.427 ns | 1.605 ns |  4.16 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 404.20 ns |  3.406 ns | 2.027 ns |  5.10 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 322.18 ns |  2.835 ns | 1.875 ns |  4.06 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 139.85 ns |  1.357 ns | 0.898 ns |  1.76 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  90.09 ns |  1.584 ns | 1.048 ns |  1.14 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 185.91 ns |  2.420 ns | 1.601 ns |  2.34 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 150.55 ns |  3.111 ns | 2.058 ns |  1.90 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  51.11 ns |  0.243 ns | 0.161 ns |  0.64 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  59.66 ns |  0.272 ns | 0.162 ns |  0.75 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  89.37 ns |  2.398 ns | 1.586 ns |  1.13 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 504.12 ns |  1.401 ns | 0.927 ns |  6.36 |    0.04 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  78.22 ns |  1.822 ns | 0.100 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 341.27 ns | 12.839 ns | 0.704 ns |  4.36 |    0.01 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 397.59 ns | 19.352 ns | 1.061 ns |  5.08 |    0.01 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 329.22 ns | 34.972 ns | 1.917 ns |  4.21 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 143.21 ns |  3.052 ns | 0.167 ns |  1.83 |    0.00 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  90.08 ns |  6.312 ns | 0.346 ns |  1.15 |    0.00 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 202.11 ns | 62.176 ns | 3.408 ns |  2.58 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 143.64 ns | 14.149 ns | 0.776 ns |  1.84 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  50.59 ns |  2.923 ns | 0.160 ns |  0.65 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  57.48 ns |  3.878 ns | 0.213 ns |  0.73 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  86.01 ns | 12.378 ns | 0.678 ns |  1.10 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 510.68 ns | 34.856 ns | 1.911 ns |  6.53 |    0.02 | 0.0229 |     392 B |        1.75 |
