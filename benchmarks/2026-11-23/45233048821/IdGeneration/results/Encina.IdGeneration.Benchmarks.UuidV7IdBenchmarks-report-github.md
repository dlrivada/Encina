```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 766.9 ns |  4.83 ns | 3.20 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 663.9 ns |  3.52 ns | 2.33 ns |  0.86 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 772.4 ns |  7.99 ns | 5.29 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 764.7 ns | 43.64 ns | 2.39 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 666.2 ns | 30.84 ns | 1.69 ns |  0.83 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 802.1 ns | 12.16 ns | 0.67 ns |  1.00 |         - |          NA |
