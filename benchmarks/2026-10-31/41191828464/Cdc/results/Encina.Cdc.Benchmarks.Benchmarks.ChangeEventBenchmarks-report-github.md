```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.690 ns | 7.6227 ns | 0.4178 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.384 ns | 0.0265 ns | 0.0015 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.737 ns | 0.4189 ns | 0.0230 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.990 ns | 1.5909 ns | 0.0872 ns |  1.00 | 0.0033 |      56 B |        1.00 |
