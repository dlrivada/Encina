```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 681.2 ns |  2.58 ns | 1.54 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 690.2 ns |  4.92 ns | 2.57 ns |  1.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 686.8 ns | 10.57 ns | 6.99 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 655.0 ns | 69.73 ns | 3.82 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 688.2 ns | 28.51 ns | 1.56 ns |  1.05 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 655.0 ns | 17.23 ns | 0.94 ns |  1.00 |         - |          NA |
