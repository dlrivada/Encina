```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.65 ns |  0.531 ns | 0.029 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.54 ns |  0.941 ns | 0.052 ns |  0.35 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 145.42 ns |  2.342 ns | 0.128 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.51 ns | 18.668 ns | 1.023 ns |  0.68 |      - |         - |        0.00 |
