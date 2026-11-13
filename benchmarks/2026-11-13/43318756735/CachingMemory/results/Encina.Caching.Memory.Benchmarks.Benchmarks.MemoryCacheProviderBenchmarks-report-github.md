```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.44 ns | 5.121 ns | 0.281 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.62 ns | 4.804 ns | 0.263 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 138.88 ns | 6.023 ns | 0.330 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 101.11 ns | 5.376 ns | 0.295 ns |  0.73 |      - |         - |        0.00 |
