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
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  99.05 ns |  1.063 ns | 0.703 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 423.82 ns |  1.718 ns | 1.136 ns |  4.28 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 502.54 ns |  2.630 ns | 1.740 ns |  5.07 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 410.45 ns |  4.149 ns | 2.745 ns |  4.14 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 186.53 ns |  3.329 ns | 2.202 ns |  1.88 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 126.84 ns |  2.095 ns | 1.385 ns |  1.28 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 240.52 ns |  2.033 ns | 1.344 ns |  2.43 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 185.49 ns |  0.583 ns | 0.347 ns |  1.87 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  57.79 ns |  0.518 ns | 0.342 ns |  0.58 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  71.28 ns |  1.170 ns | 0.774 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 108.66 ns |  3.126 ns | 2.068 ns |  1.10 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 608.41 ns |  3.103 ns | 1.623 ns |  6.14 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 102.95 ns | 32.632 ns | 1.789 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 428.12 ns | 16.085 ns | 0.882 ns |  4.16 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 529.98 ns | 92.192 ns | 5.053 ns |  5.15 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 427.30 ns | 55.832 ns | 3.060 ns |  4.15 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 188.96 ns |  5.494 ns | 0.301 ns |  1.84 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 130.01 ns |  4.182 ns | 0.229 ns |  1.26 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 236.49 ns | 36.924 ns | 2.024 ns |  2.30 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 185.61 ns |  4.621 ns | 0.253 ns |  1.80 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  58.26 ns |  2.106 ns | 0.115 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  71.59 ns |  2.828 ns | 0.155 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 115.89 ns | 43.717 ns | 2.396 ns |  1.13 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 592.38 ns | 33.731 ns | 1.849 ns |  5.76 |    0.09 | 0.0248 |     416 B |        1.86 |
