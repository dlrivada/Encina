```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.570 ns | 13.3468 ns | 0.7316 ns |  0.65 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.389 ns |  0.1584 ns | 0.0087 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.511 ns |  5.8056 ns | 0.3182 ns |  0.36 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.923 ns |  3.8310 ns | 0.2100 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
