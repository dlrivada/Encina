```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.943 ns | 0.0349 ns | 0.0500 ns |  0.61 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.381 ns | 0.0168 ns | 0.0242 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.978 ns | 0.1310 ns | 0.1960 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.338 ns | 0.1911 ns | 0.2801 ns |  1.00 | 0.0033 |      56 B |        1.00 |
