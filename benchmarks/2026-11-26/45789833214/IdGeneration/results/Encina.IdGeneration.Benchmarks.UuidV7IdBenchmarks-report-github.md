```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 772.8 ns | 17.67 ns | 11.69 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 663.3 ns |  2.28 ns |  1.36 ns |  0.86 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 767.2 ns |  2.59 ns |  1.54 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 766.6 ns | 78.79 ns |  4.32 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 664.9 ns |  7.71 ns |  0.42 ns |  0.86 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 772.6 ns | 25.82 ns |  1.42 ns |  1.00 |         - |          NA |
