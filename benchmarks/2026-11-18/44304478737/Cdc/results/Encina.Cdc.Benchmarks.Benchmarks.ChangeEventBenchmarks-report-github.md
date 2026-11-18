```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 29.274 ns | 1.8066 ns | 0.0990 ns |  0.85 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.720 ns | 0.4274 ns | 0.0234 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 16.251 ns | 3.4025 ns | 0.1865 ns |  0.47 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.375 ns | 2.3179 ns | 0.1271 ns |  1.00 | 0.0007 |      56 B |        1.00 |
