```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  53.89 ns |   2.629 ns |  1.739 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 225.69 ns |   9.859 ns |  6.521 ns |  4.19 |    0.17 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 288.56 ns |  15.494 ns | 10.248 ns |  5.36 |    0.24 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 231.77 ns |   5.824 ns |  3.466 ns |  4.30 |    0.14 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 103.08 ns |   1.257 ns |  0.748 ns |  1.91 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  62.52 ns |   2.183 ns |  1.299 ns |  1.16 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 134.17 ns |   1.466 ns |  0.872 ns |  2.49 |    0.08 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 101.18 ns |   1.333 ns |  0.697 ns |  1.88 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  38.96 ns |   1.013 ns |  0.603 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  42.08 ns |   0.782 ns |  0.517 ns |  0.78 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  61.61 ns |   1.902 ns |  1.258 ns |  1.14 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 366.99 ns |   6.025 ns |  3.585 ns |  6.82 |    0.22 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  62.65 ns |  15.034 ns |  0.824 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 251.54 ns | 109.131 ns |  5.982 ns |  4.02 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 300.14 ns |  47.096 ns |  2.582 ns |  4.79 |    0.07 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 239.66 ns | 122.276 ns |  6.702 ns |  3.83 |    0.10 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 107.28 ns |  26.449 ns |  1.450 ns |  1.71 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  65.06 ns |   5.133 ns |  0.281 ns |  1.04 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 136.98 ns |  25.277 ns |  1.385 ns |  2.19 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 102.45 ns |  36.052 ns |  1.976 ns |  1.64 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  39.95 ns |  22.600 ns |  1.239 ns |  0.64 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  40.72 ns |  15.119 ns |  0.829 ns |  0.65 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  61.76 ns |  43.342 ns |  2.376 ns |  0.99 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 364.62 ns | 129.054 ns |  7.074 ns |  5.82 |    0.12 | 0.0234 |     392 B |        1.75 |
