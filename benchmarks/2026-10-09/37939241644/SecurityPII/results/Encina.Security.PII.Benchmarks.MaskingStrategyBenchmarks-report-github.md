```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  53.27 ns |   1.938 ns |  1.153 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 243.92 ns |  17.453 ns | 11.544 ns |  4.58 |    0.23 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 287.90 ns |  14.860 ns |  9.829 ns |  5.41 |    0.21 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 226.77 ns |   6.160 ns |  3.666 ns |  4.26 |    0.11 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 103.54 ns |   5.119 ns |  3.386 ns |  1.94 |    0.07 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  61.36 ns |   2.695 ns |  1.604 ns |  1.15 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 128.29 ns |   5.770 ns |  3.434 ns |  2.41 |    0.08 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  96.60 ns |   2.343 ns |  1.394 ns |  1.81 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  38.55 ns |   2.921 ns |  1.932 ns |  0.72 |    0.04 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  39.44 ns |   1.355 ns |  0.806 ns |  0.74 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  55.01 ns |   1.207 ns |  0.631 ns |  1.03 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 339.85 ns |  10.540 ns |  6.272 ns |  6.38 |    0.17 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  51.48 ns |  15.111 ns |  0.828 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 233.69 ns | 109.203 ns |  5.986 ns |  4.54 |    0.12 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 278.19 ns | 111.459 ns |  6.109 ns |  5.40 |    0.13 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 240.04 ns |  32.856 ns |  1.801 ns |  4.66 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 102.58 ns |  51.210 ns |  2.807 ns |  1.99 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  60.16 ns |  14.964 ns |  0.820 ns |  1.17 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 125.18 ns |  59.524 ns |  3.263 ns |  2.43 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  98.71 ns |  10.965 ns |  0.601 ns |  1.92 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  37.99 ns |   7.646 ns |  0.419 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  41.08 ns |  56.600 ns |  3.102 ns |  0.80 |    0.05 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  56.65 ns |  14.642 ns |  0.803 ns |  1.10 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 340.58 ns |  31.797 ns |  1.743 ns |  6.62 |    0.10 | 0.0234 |     392 B |        1.75 |
