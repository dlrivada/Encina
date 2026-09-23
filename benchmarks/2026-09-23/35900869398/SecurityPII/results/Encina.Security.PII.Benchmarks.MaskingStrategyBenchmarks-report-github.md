```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  92.29 ns |  0.150 ns | 0.079 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 398.53 ns |  1.702 ns | 1.013 ns |  4.32 |    0.01 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 480.93 ns |  3.805 ns | 2.517 ns |  5.21 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 385.30 ns |  1.246 ns | 0.824 ns |  4.17 |    0.01 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 174.60 ns |  0.277 ns | 0.165 ns |  1.89 |    0.00 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 114.73 ns |  0.544 ns | 0.360 ns |  1.24 |    0.00 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 217.44 ns |  0.784 ns | 0.518 ns |  2.36 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 180.35 ns |  0.344 ns | 0.227 ns |  1.95 |    0.00 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  52.12 ns |  0.114 ns | 0.075 ns |  0.56 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  65.19 ns |  0.118 ns | 0.062 ns |  0.71 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 100.43 ns |  0.514 ns | 0.340 ns |  1.09 |    0.00 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 570.22 ns |  1.456 ns | 0.866 ns |  6.18 |    0.01 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  90.52 ns |  1.090 ns | 0.060 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 410.04 ns | 19.297 ns | 1.058 ns |  4.53 |    0.01 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 492.86 ns | 35.277 ns | 1.934 ns |  5.44 |    0.02 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 391.43 ns |  8.992 ns | 0.493 ns |  4.32 |    0.01 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 177.28 ns | 15.106 ns | 0.828 ns |  1.96 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 115.41 ns |  1.392 ns | 0.076 ns |  1.27 |    0.00 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 226.15 ns | 13.464 ns | 0.738 ns |  2.50 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 172.55 ns | 11.979 ns | 0.657 ns |  1.91 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  53.35 ns | 19.615 ns | 1.075 ns |  0.59 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  67.11 ns |  3.151 ns | 0.173 ns |  0.74 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  98.12 ns |  8.966 ns | 0.491 ns |  1.08 |    0.00 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 566.71 ns | 10.828 ns | 0.594 ns |  6.26 |    0.01 | 0.0248 |     416 B |        1.86 |
