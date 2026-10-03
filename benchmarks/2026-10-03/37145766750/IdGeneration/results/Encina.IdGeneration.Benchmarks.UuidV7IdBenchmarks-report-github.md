```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 678.6 ns |  11.46 ns |  7.58 ns |  0.97 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 442.8 ns |   6.18 ns |  4.09 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 700.8 ns |  22.82 ns | 15.09 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 683.1 ns | 125.02 ns |  6.85 ns |  1.01 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 434.8 ns |  10.46 ns |  0.57 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 678.7 ns |  34.27 ns |  1.88 ns |  1.00 |    0.00 |         - |          NA |
