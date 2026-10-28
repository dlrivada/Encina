```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  56.45 ns | 5.275 ns | 0.289 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  64.84 ns | 0.661 ns | 0.036 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 176.28 ns | 4.236 ns | 0.232 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 124.50 ns | 2.289 ns | 0.125 ns |  0.71 |      - |         - |        0.00 |
