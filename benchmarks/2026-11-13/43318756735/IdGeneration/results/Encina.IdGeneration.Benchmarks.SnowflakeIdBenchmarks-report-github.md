```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 241.9 ns | 0.02 ns | 0.01 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 241.8 ns | 0.09 ns | 0.06 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 241.8 ns | 0.12 ns | 0.08 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 240.0 ns | 0.05 ns | 0.03 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 241.8 ns | 2.23 ns | 0.12 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.7 ns | 2.26 ns | 0.12 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 241.8 ns | 1.79 ns | 0.10 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 240.2 ns | 1.59 ns | 0.09 ns |  0.99 |         - |          NA |
