```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  72.46 ns |  2.783 ns | 1.841 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 342.39 ns | 11.577 ns | 7.658 ns |  4.73 |    0.15 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 408.09 ns | 10.998 ns | 7.275 ns |  5.64 |    0.17 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 323.46 ns |  7.368 ns | 4.874 ns |  4.47 |    0.13 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 129.94 ns |  0.895 ns | 0.592 ns |  1.79 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  78.46 ns |  0.789 ns | 0.470 ns |  1.08 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 167.20 ns |  2.457 ns | 1.462 ns |  2.31 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 133.42 ns |  2.771 ns | 1.833 ns |  1.84 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  42.79 ns |  0.446 ns | 0.234 ns |  0.59 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  50.22 ns |  0.584 ns | 0.387 ns |  0.69 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  78.16 ns |  2.137 ns | 1.413 ns |  1.08 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 430.19 ns |  3.491 ns | 2.309 ns |  5.94 |    0.15 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  70.44 ns |  7.591 ns | 0.416 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 328.63 ns | 41.882 ns | 2.296 ns |  4.67 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 397.18 ns | 31.251 ns | 1.713 ns |  5.64 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 322.40 ns | 16.002 ns | 0.877 ns |  4.58 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 128.66 ns |  1.259 ns | 0.069 ns |  1.83 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  78.72 ns | 14.150 ns | 0.776 ns |  1.12 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 171.12 ns |  8.625 ns | 0.473 ns |  2.43 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 127.91 ns | 15.840 ns | 0.868 ns |  1.82 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  43.54 ns |  3.971 ns | 0.218 ns |  0.62 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  50.61 ns | 12.746 ns | 0.699 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  75.64 ns |  8.707 ns | 0.477 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 443.48 ns | 24.292 ns | 1.332 ns |  6.30 |    0.04 | 0.0248 |     416 B |        1.86 |
