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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 684.4 ns |  12.68 ns |  7.55 ns |  1.03 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 436.5 ns |  14.04 ns |  9.28 ns |  0.66 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 663.8 ns |   9.06 ns |  5.99 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 706.1 ns | 300.39 ns | 16.47 ns |  1.04 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 442.8 ns |  81.40 ns |  4.46 ns |  0.65 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 678.7 ns |   8.77 ns |  0.48 ns |  1.00 |    0.00 |         - |          NA |
