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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 762.9 ns |  3.72 ns | 2.46 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 671.2 ns |  2.31 ns | 1.52 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 769.0 ns |  0.84 ns | 0.44 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 766.1 ns | 34.05 ns | 1.87 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 672.1 ns | 21.45 ns | 1.18 ns |  0.88 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 764.2 ns | 47.32 ns | 2.59 ns |  1.00 |         - |          NA |
