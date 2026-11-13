```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 431.8 ns |  1.15 ns | 0.76 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 328.9 ns |  2.24 ns | 1.48 ns |  0.76 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 435.0 ns |  1.48 ns | 0.88 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 434.4 ns | 15.90 ns | 0.87 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 327.7 ns |  2.15 ns | 0.12 ns |  0.75 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 434.8 ns |  2.73 ns | 0.15 ns |  1.00 |         - |          NA |
