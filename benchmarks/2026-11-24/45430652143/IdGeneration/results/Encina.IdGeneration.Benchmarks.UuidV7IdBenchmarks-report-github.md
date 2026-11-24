```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 433.4 ns |  1.90 ns | 1.26 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 328.1 ns |  1.34 ns | 0.89 ns |  0.76 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 434.5 ns |  0.96 ns | 0.57 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 431.9 ns |  9.12 ns | 0.50 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 328.5 ns |  2.08 ns | 0.11 ns |  0.73 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 452.4 ns | 11.15 ns | 0.61 ns |  1.00 |         - |          NA |
