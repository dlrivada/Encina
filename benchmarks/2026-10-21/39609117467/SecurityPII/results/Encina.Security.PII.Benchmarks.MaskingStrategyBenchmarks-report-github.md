```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  56.81 ns |   2.006 ns |  1.327 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 236.39 ns |   5.020 ns |  3.320 ns |  4.16 |    0.11 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 277.19 ns |   5.307 ns |  3.510 ns |  4.88 |    0.13 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 229.50 ns |   8.178 ns |  5.409 ns |  4.04 |    0.13 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 106.23 ns |   3.627 ns |  2.399 ns |  1.87 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  65.17 ns |   2.824 ns |  1.868 ns |  1.15 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 133.24 ns |   4.652 ns |  3.077 ns |  2.35 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  97.16 ns |   2.284 ns |  1.511 ns |  1.71 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  39.30 ns |   1.240 ns |  0.820 ns |  0.69 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  41.05 ns |   1.014 ns |  0.671 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  59.56 ns |   1.489 ns |  0.985 ns |  1.05 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 374.63 ns |  10.091 ns |  6.675 ns |  6.60 |    0.19 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  57.00 ns |  57.002 ns |  3.124 ns |  1.00 |    0.07 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 262.47 ns | 547.095 ns | 29.988 ns |  4.61 |    0.50 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 301.35 ns | 167.605 ns |  9.187 ns |  5.30 |    0.28 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 240.22 ns |  69.413 ns |  3.805 ns |  4.22 |    0.20 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 108.24 ns |  35.647 ns |  1.954 ns |  1.90 |    0.09 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  64.20 ns |  20.140 ns |  1.104 ns |  1.13 |    0.05 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 133.54 ns |  34.979 ns |  1.917 ns |  2.35 |    0.11 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 103.55 ns |  20.834 ns |  1.142 ns |  1.82 |    0.09 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  38.50 ns |  14.598 ns |  0.800 ns |  0.68 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  42.65 ns |  23.118 ns |  1.267 ns |  0.75 |    0.04 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  63.25 ns |  78.041 ns |  4.278 ns |  1.11 |    0.08 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 369.69 ns |  89.120 ns |  4.885 ns |  6.50 |    0.31 | 0.0234 |     392 B |        1.75 |
