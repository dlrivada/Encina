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
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  97.78 ns |  0.599 ns | 0.396 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 414.58 ns |  1.156 ns | 0.688 ns |  4.24 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 501.60 ns |  7.100 ns | 4.697 ns |  5.13 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 407.60 ns |  6.787 ns | 4.489 ns |  4.17 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 184.61 ns |  0.794 ns | 0.525 ns |  1.89 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 121.75 ns |  1.116 ns | 0.664 ns |  1.25 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 238.05 ns |  1.906 ns | 1.261 ns |  2.43 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 181.84 ns |  0.443 ns | 0.264 ns |  1.86 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  55.88 ns |  0.169 ns | 0.112 ns |  0.57 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  69.12 ns |  0.522 ns | 0.345 ns |  0.71 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 106.98 ns |  1.397 ns | 0.924 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 594.08 ns |  0.937 ns | 0.490 ns |  6.08 |    0.02 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.90 ns |  4.357 ns | 0.239 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 428.58 ns | 49.906 ns | 2.736 ns |  4.42 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 511.29 ns | 38.294 ns | 2.099 ns |  5.28 |    0.02 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 404.73 ns | 57.040 ns | 3.127 ns |  4.18 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 178.68 ns | 18.758 ns | 1.028 ns |  1.84 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 115.05 ns |  9.665 ns | 0.530 ns |  1.19 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 222.43 ns | 42.770 ns | 2.344 ns |  2.30 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 181.85 ns | 15.508 ns | 0.850 ns |  1.88 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.57 ns |  5.622 ns | 0.308 ns |  0.56 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  72.30 ns | 10.659 ns | 0.584 ns |  0.75 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 102.55 ns |  6.162 ns | 0.338 ns |  1.06 |    0.00 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 596.12 ns | 32.267 ns | 1.769 ns |  6.15 |    0.02 | 0.0248 |     416 B |        1.86 |
