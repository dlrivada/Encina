```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 763.0 ns |  3.85 ns | 2.54 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 664.7 ns |  1.28 ns | 0.76 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 768.0 ns |  4.05 ns | 2.68 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 801.9 ns | 92.10 ns | 5.05 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 663.6 ns | 19.63 ns | 1.08 ns |  0.86 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 773.5 ns | 94.54 ns | 5.18 ns |  1.00 |         - |          NA |
