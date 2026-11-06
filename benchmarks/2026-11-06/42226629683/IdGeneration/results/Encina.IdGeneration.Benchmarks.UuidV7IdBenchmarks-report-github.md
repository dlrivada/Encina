```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 393.9 ns |  0.94 ns | 0.49 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 309.2 ns |  0.23 ns | 0.12 ns |  0.78 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 395.9 ns |  2.03 ns | 1.06 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 411.3 ns | 15.40 ns | 0.84 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 309.2 ns |  3.54 ns | 0.19 ns |  0.76 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 409.2 ns |  7.97 ns | 0.44 ns |  1.00 |         - |          NA |
