```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   938.8 ns | 67.33 ns | 3.69 ns |  0.80 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   974.2 ns | 44.26 ns | 2.43 ns |  0.83 | 0.0057 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,177.4 ns | 65.11 ns | 3.57 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   909.9 ns | 26.44 ns | 1.45 ns |  0.77 | 0.0067 |     616 B |        1.24 |
