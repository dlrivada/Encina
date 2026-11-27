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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 665.0 ns |  5.44 ns | 3.60 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 681.5 ns |  5.13 ns | 3.39 ns |  1.03 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 663.3 ns |  5.78 ns | 3.44 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 667.3 ns | 31.91 ns | 1.75 ns |  0.97 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 678.7 ns |  1.10 ns | 0.06 ns |  0.99 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 686.7 ns | 33.05 ns | 1.81 ns |  1.00 |         - |          NA |
