```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 29.240 ns | 1.5305 ns | 0.0839 ns |  0.81 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.870 ns | 0.4335 ns | 0.0238 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 15.950 ns | 0.3427 ns | 0.0188 ns |  0.44 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.182 ns | 1.3581 ns | 0.0744 ns |  1.00 | 0.0007 |      56 B |        1.00 |
