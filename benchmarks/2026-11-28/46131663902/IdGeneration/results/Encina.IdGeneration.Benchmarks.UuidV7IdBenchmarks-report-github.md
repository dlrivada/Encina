```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 768.6 ns |  4.09 ns | 2.14 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 662.7 ns |  0.60 ns | 0.36 ns |  0.82 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 805.2 ns |  4.71 ns | 3.11 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 805.7 ns | 67.66 ns | 3.71 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 665.4 ns | 12.69 ns | 0.70 ns |  0.82 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 806.8 ns | 13.93 ns | 0.76 ns |  1.00 |         - |          NA |
