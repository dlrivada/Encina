```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.754 ns | 22.5328 ns | 1.2351 ns |  0.68 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.383 ns |  0.0020 ns | 0.0001 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.045 ns |  5.1555 ns | 0.2826 ns |  0.36 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.350 ns |  9.9589 ns | 0.5459 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
