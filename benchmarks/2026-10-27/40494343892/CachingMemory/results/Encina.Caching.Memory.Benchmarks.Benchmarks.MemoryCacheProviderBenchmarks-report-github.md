```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  48.95 ns | 18.15 ns | 0.995 ns |  0.33 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  53.21 ns | 23.01 ns | 1.261 ns |  0.36 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 148.41 ns | 86.90 ns | 4.763 ns |  1.00 |    0.04 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 102.30 ns | 46.33 ns | 2.539 ns |  0.69 |    0.02 |      - |         - |        0.00 |
