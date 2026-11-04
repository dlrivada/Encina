```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.741 ns | 12.6802 ns | 0.6950 ns |  0.65 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.247 ns |  0.6468 ns | 0.0355 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.168 ns |  5.7607 ns | 0.3158 ns |  0.39 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.262 ns |  1.8858 ns | 0.1034 ns |  1.00 |    0.00 | 0.0033 |      56 B |        1.00 |
