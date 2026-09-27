```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.76 ns |      9.698 ns |   0.532 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.51 ns |      1.174 ns |   0.064 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,989.49 ns | 12,915.521 ns | 707.943 ns | 2,314.68 |   21.98 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.88 ns |     13.504 ns |   0.740 ns |     4.56 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.95 ns |      7.723 ns |   0.423 ns |     3.01 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
