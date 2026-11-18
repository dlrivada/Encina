```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 242.3 ns |  0.18 ns | 0.12 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 242.4 ns |  0.16 ns | 0.10 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 242.4 ns |  0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 241.0 ns |  0.18 ns | 0.12 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |          |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 242.4 ns |  2.13 ns | 0.12 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 242.7 ns | 11.30 ns | 0.62 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 242.3 ns |  3.06 ns | 0.17 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 241.4 ns |  2.30 ns | 0.13 ns |  1.00 |         - |          NA |
