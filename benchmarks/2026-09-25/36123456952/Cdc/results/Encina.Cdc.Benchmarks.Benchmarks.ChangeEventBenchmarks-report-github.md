```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 19.059 ns | 1.0489 ns | 0.0575 ns |  0.62 |    0.01 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.423 ns | 0.1066 ns | 0.0058 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.627 ns | 5.8635 ns | 0.3214 ns |  0.35 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 30.717 ns | 7.8638 ns | 0.4310 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |
