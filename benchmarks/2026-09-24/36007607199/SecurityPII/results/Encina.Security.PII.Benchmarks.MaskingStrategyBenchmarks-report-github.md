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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 100.77 ns |  1.922 ns | 1.144 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 418.93 ns |  3.118 ns | 2.062 ns |  4.16 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 506.66 ns |  7.803 ns | 5.161 ns |  5.03 |    0.07 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 401.35 ns |  2.572 ns | 1.345 ns |  3.98 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 185.42 ns |  1.190 ns | 0.708 ns |  1.84 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 125.74 ns |  0.792 ns | 0.471 ns |  1.25 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 230.51 ns |  1.710 ns | 1.017 ns |  2.29 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 181.07 ns |  3.307 ns | 2.187 ns |  1.80 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  60.62 ns |  1.092 ns | 0.722 ns |  0.60 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  67.25 ns |  1.058 ns | 0.700 ns |  0.67 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 104.57 ns |  4.981 ns | 3.295 ns |  1.04 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 577.84 ns |  4.242 ns | 2.806 ns |  5.74 |    0.07 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  99.94 ns | 18.221 ns | 0.999 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 419.85 ns | 66.457 ns | 3.643 ns |  4.20 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 490.74 ns | 71.255 ns | 3.906 ns |  4.91 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 410.87 ns | 48.346 ns | 2.650 ns |  4.11 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 186.28 ns |  7.810 ns | 0.428 ns |  1.86 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 123.63 ns | 34.308 ns | 1.881 ns |  1.24 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 222.48 ns | 46.087 ns | 2.526 ns |  2.23 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 184.55 ns | 12.646 ns | 0.693 ns |  1.85 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  56.57 ns |  6.478 ns | 0.355 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  67.89 ns |  4.535 ns | 0.249 ns |  0.68 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 108.63 ns | 13.713 ns | 0.752 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 605.70 ns | 34.339 ns | 1.882 ns |  6.06 |    0.06 | 0.0248 |     416 B |        1.86 |
