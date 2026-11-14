```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 753.1 ns |  8.90 ns | 5.89 ns |  0.96 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 664.1 ns |  2.76 ns | 1.83 ns |  0.85 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 785.5 ns |  1.29 ns | 0.77 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 745.9 ns | 15.15 ns | 0.83 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 666.0 ns | 36.00 ns | 1.97 ns |  0.89 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 746.2 ns | 17.31 ns | 0.95 ns |  1.00 |         - |          NA |
