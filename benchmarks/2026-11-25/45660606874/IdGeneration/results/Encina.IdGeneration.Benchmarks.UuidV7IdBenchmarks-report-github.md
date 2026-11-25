```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 761.1 ns |  1.80 ns | 1.19 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 663.5 ns |  2.68 ns | 1.77 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 766.0 ns |  4.96 ns | 3.28 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 799.5 ns | 30.45 ns | 1.67 ns |  1.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 663.2 ns |  2.21 ns | 0.12 ns |  0.86 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 774.3 ns | 82.00 ns | 4.49 ns |  1.00 |         - |          NA |
