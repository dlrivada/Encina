```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  98.02 ns |   0.524 ns | 0.347 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 441.70 ns |  14.985 ns | 9.911 ns |  4.51 |    0.10 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 507.78 ns |   4.003 ns | 2.382 ns |  5.18 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 421.05 ns |   2.738 ns | 1.811 ns |  4.30 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 182.59 ns |   1.283 ns | 0.848 ns |  1.86 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 110.99 ns |   0.875 ns | 0.579 ns |  1.13 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 230.99 ns |   2.102 ns | 1.251 ns |  2.36 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 172.11 ns |   1.239 ns | 0.737 ns |  1.76 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  64.23 ns |   0.408 ns | 0.243 ns |  0.66 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  72.88 ns |   0.525 ns | 0.347 ns |  0.74 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 106.61 ns |   0.879 ns | 0.582 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 604.56 ns |   4.438 ns | 2.321 ns |  6.17 |    0.03 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  98.83 ns |   3.907 ns | 0.214 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 426.43 ns |  30.814 ns | 1.689 ns |  4.31 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 524.87 ns | 102.952 ns | 5.643 ns |  5.31 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 420.05 ns |  31.458 ns | 1.724 ns |  4.25 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 176.42 ns |  11.518 ns | 0.631 ns |  1.79 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 112.66 ns |  14.189 ns | 0.778 ns |  1.14 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 234.25 ns |  30.292 ns | 1.660 ns |  2.37 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 172.72 ns |   2.138 ns | 0.117 ns |  1.75 |    0.00 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  64.23 ns |   5.344 ns | 0.293 ns |  0.65 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  74.35 ns |   4.051 ns | 0.222 ns |  0.75 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 107.21 ns |  13.292 ns | 0.729 ns |  1.08 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 587.45 ns |  37.439 ns | 2.052 ns |  5.94 |    0.02 | 0.0229 |     392 B |        1.75 |
