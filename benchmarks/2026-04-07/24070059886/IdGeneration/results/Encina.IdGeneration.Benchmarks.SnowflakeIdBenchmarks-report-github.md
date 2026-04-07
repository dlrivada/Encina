```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|----------:|------------:|
| Generate             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        241.9 ns | 0.10 ns | 0.07 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        241.9 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        240.3 ns | 0.09 ns | 0.06 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        241.9 ns | 0.07 ns | 0.05 ns |  1.00 |         - |          NA |
|                      |            |                |             |             |              |             |                 |         |         |       |           |             |
| Generate             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,531,134.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,029,274.0 ns |      NA | 0.00 ns |  1.44 |         - |          NA |
| ExtractShardId       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 19,925,009.0 ns |      NA | 0.00 ns |  1.59 |         - |          NA |
| GenerateAndGetValue  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,880,910.0 ns |      NA | 0.00 ns |  1.03 |         - |          NA |
