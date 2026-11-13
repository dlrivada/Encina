```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 490.5 ns |  6.92 ns | 4.58 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 420.4 ns |  0.87 ns | 0.58 ns |  0.85 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 493.7 ns |  8.23 ns | 4.90 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 483.9 ns | 41.84 ns | 2.29 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 420.6 ns |  7.58 ns | 0.42 ns |  0.86 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 486.5 ns | 46.81 ns | 2.57 ns |  1.00 |         - |          NA |
