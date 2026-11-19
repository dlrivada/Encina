```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.17 ns |  1.480 ns | 0.081 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.35 ns |  6.251 ns | 0.343 ns |  0.34 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 146.04 ns |  9.274 ns | 0.508 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  99.41 ns | 41.816 ns | 2.292 ns |  0.68 |      - |         - |        0.00 |
