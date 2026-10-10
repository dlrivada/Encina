```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 658.9 ns |   2.55 ns |  1.68 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 328.1 ns |   0.35 ns |  0.23 ns |  0.50 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 661.6 ns |   5.00 ns |  2.61 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 737.3 ns | 844.65 ns | 46.30 ns |  1.00 |    0.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 325.1 ns |   6.33 ns |  0.35 ns |  0.44 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 741.9 ns | 714.11 ns | 39.14 ns |  1.00 |    0.07 |         - |          NA |
