```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.76GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 613.6 ns |  1.27 ns | 0.66 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 392.6 ns |  0.28 ns | 0.18 ns |  0.64 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 615.3 ns |  2.17 ns | 1.29 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 611.2 ns |  7.25 ns | 0.40 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 392.4 ns | 21.20 ns | 1.16 ns |  0.64 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 615.5 ns | 53.13 ns | 2.91 ns |  1.00 |         - |          NA |
