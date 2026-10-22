```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.041 ns | 10.3632 ns | 0.5680 ns |  0.62 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.370 ns |  0.4386 ns | 0.0240 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.472 ns | 11.7067 ns | 0.6417 ns |  0.35 |    0.02 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 32.575 ns |  8.1745 ns | 0.4481 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |
