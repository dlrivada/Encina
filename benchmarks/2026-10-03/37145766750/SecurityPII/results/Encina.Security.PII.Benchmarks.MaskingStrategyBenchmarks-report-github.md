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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 111.12 ns |  1.387 ns | 0.917 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 427.35 ns |  5.209 ns | 3.100 ns |  3.85 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 511.74 ns |  4.045 ns | 2.676 ns |  4.61 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 414.55 ns |  3.647 ns | 2.412 ns |  3.73 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 194.37 ns |  2.566 ns | 1.697 ns |  1.75 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 131.18 ns |  2.543 ns | 1.682 ns |  1.18 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 241.27 ns |  5.004 ns | 3.310 ns |  2.17 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 192.43 ns |  3.903 ns | 2.582 ns |  1.73 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  64.04 ns |  0.783 ns | 0.518 ns |  0.58 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  77.76 ns |  1.314 ns | 0.869 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 112.66 ns |  1.263 ns | 0.751 ns |  1.01 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 566.61 ns |  2.263 ns | 1.497 ns |  5.10 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 103.94 ns | 34.384 ns | 1.885 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 422.02 ns | 27.402 ns | 1.502 ns |  4.06 |    0.07 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 506.12 ns | 35.892 ns | 1.967 ns |  4.87 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 413.80 ns | 38.072 ns | 2.087 ns |  3.98 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 186.09 ns | 18.395 ns | 1.008 ns |  1.79 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 126.55 ns | 18.946 ns | 1.039 ns |  1.22 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 235.35 ns | 50.284 ns | 2.756 ns |  2.26 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 191.36 ns | 31.072 ns | 1.703 ns |  1.84 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  67.77 ns |  7.759 ns | 0.425 ns |  0.65 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  77.25 ns |  8.733 ns | 0.479 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 114.33 ns | 30.479 ns | 1.671 ns |  1.10 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 583.73 ns | 35.655 ns | 1.954 ns |  5.62 |    0.09 | 0.0248 |     416 B |        1.86 |
