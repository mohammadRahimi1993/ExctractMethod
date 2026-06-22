Re-implementation of the paper "Identification of Extract Method Refactoring Opportunities for the Decomposition of Methods" (JSS, 2011).

## ABSTRACT
</br>
<div align="justify">
The extraction of a code fragment into a separate method is one of the most widely performed refactoring activities, since it allows the decomposition of large and complex methods and can be used in combination with other code transformations for fixing a variety of design problems. Despite the significance of Extract Method refactoring towards code quality improvement, there is limited support for the identification of code fragments with distinct functionality that could be extracted into new methods. The goal of our approach is to automatically identify Extract Method refactoring opportunities which are related with the complete computation of a given variable (complete computation slice) and the statements affecting the state of a given object (object state slice). Moreover, a set of rules regarding the preservation of existing dependences is proposed that exclude refactoring opportunities corresponding to slices whose extraction could possibly cause a change in program behavior. The proposed approach has been evaluated regarding its ability to capture slices of code implementing a distinct functionality, its ability to resolve existing design flaws, its impact on the cohesion of the decomposed and extracted methods, and its ability to preserve program behavior. Moreover, precision and recall have been computed employing the refactoring opportunities found by independent evaluators in software that they developed as a golden set.
</div>

## BibTeX
```bibtex
@article{ title={Identification of extract method refactoring opportunities for the decomposition of methods},
          author={Tsantalis, Nikolaos and Chatzigeorgiou, Alexander},
          journal={Journal of Systems and Software},
          volume={84},
          number={10},
          pages={1757--1782},
          year={2011},
          publisher={Elsevier},
          doi={10.1016/j.jss.2011.05.016}
}

