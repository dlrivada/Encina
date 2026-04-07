```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  96.79 ns | 0.950 ns | 0.629 ns |  96.82 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 427.36 ns | 1.483 ns | 0.981 ns | 427.16 ns |  4.42 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 487.26 ns | 3.239 ns | 2.143 ns | 487.41 ns |  5.03 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 402.11 ns | 4.490 ns | 2.672 ns | 401.82 ns |  4.15 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 178.62 ns | 0.786 ns | 0.411 ns | 178.58 ns |  1.85 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 118.86 ns | 1.164 ns | 0.692 ns | 118.64 ns |  1.23 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 223.07 ns | 2.846 ns | 1.882 ns | 222.76 ns |  2.30 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 181.35 ns | 1.273 ns | 0.842 ns | 181.41 ns |  1.87 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  54.59 ns | 0.336 ns | 0.222 ns |  54.60 ns |  0.56 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  68.97 ns | 0.479 ns | 0.251 ns |  69.04 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 103.98 ns | 0.590 ns | 0.351 ns | 103.96 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 592.36 ns | 2.209 ns | 1.315 ns | 592.69 ns |  6.12 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |          |          |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  97.36 ns | 0.213 ns | 0.312 ns |  97.34 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 419.97 ns | 3.034 ns | 4.253 ns | 417.07 ns |  4.31 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 487.77 ns | 2.724 ns | 4.077 ns | 486.56 ns |  5.01 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 407.28 ns | 1.619 ns | 2.217 ns | 406.93 ns |  4.18 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 186.61 ns | 3.277 ns | 4.486 ns | 186.62 ns |  1.92 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 119.30 ns | 0.682 ns | 1.000 ns | 119.03 ns |  1.23 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 226.64 ns | 0.830 ns | 1.190 ns | 226.69 ns |  2.33 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 177.52 ns | 0.529 ns | 0.792 ns | 177.66 ns |  1.82 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  55.41 ns | 1.046 ns | 1.533 ns |  56.22 ns |  0.57 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  69.08 ns | 0.363 ns | 0.544 ns |  69.06 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 104.02 ns | 0.413 ns | 0.605 ns | 103.96 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 596.61 ns | 3.277 ns | 4.905 ns | 597.31 ns |  6.13 |    0.05 | 0.0248 |     416 B |        1.86 |
