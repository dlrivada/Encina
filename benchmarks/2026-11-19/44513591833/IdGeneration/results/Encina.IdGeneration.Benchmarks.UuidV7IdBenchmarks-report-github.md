```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 615.6 ns |  3.53 ns | 2.10 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 401.1 ns |  1.41 ns | 0.84 ns |  0.65 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 620.1 ns |  7.36 ns | 4.87 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 616.9 ns | 43.51 ns | 2.39 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 400.1 ns |  9.78 ns | 0.54 ns |  0.65 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 611.5 ns | 50.97 ns | 2.79 ns |  1.00 |         - |          NA |
