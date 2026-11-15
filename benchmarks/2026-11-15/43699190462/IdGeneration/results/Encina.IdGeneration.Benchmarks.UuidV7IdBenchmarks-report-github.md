```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 773.3 ns |  4.68 ns | 3.10 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 666.6 ns |  2.53 ns | 1.51 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 765.7 ns |  0.98 ns | 0.51 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 805.3 ns | 34.20 ns | 1.87 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 668.0 ns | 10.89 ns | 0.60 ns |  0.87 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 763.7 ns | 80.36 ns | 4.40 ns |  1.00 |         - |          NA |
