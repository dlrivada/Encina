```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.109 ns | 20.3422 ns | 1.1150 ns |  0.63 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.380 ns |  0.0243 ns | 0.0013 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.387 ns |  5.7938 ns | 0.3176 ns |  0.35 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.340 ns |  8.1857 ns | 0.4487 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
